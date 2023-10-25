;; invoke: block
;; expect: (i32:42)

(module
    (func (export "block") (result i32) (local $x i32)
        i32.const 0
        local.set $x
        block $b
            i32.const 42
            local.set $x
        end
        local.get $x
    )
)