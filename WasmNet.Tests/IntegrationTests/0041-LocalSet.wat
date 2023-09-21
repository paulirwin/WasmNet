;; invoke: localset
;; expect: (i32:1)

(module
    (func (export "localset") (result i32) (local $x i32)
        i32.const 3
        local.set $x
        local.get $x
        i32.const 2
        i32.sub
    )
)