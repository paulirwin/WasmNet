;; invoke: loop
;; expect: (i32:42)

(module
    (func (export "loop") (result i32) (local $x i32)
        i32.const 0
        local.set $x
        loop $loop
            local.get $x
            i32.const 1
            i32.add
            local.tee $x
            i32.const 42
            i32.lt_s
            br_if $loop
        end
        local.get $x
    )
)