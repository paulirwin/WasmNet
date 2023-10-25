;; invoke: localtee
;; expect: (i32:1)

(module
    (func (export "localtee") (result i32) (local $x i32)
        i32.const 3
        local.tee $x
        i32.const 2
        i32.sub
    )
)